#include "Pool.h"
#include <iostream>


using namespace std;


MemoryPool::MemoryPool(unsigned int poolSize) : poolSize(poolSize)
{
	pool = new unsigned char[poolSize];
	blocks.push_back({ 0, poolSize, false });
}

void * MemoryPool::Allocate(unsigned int nBytes)
{
	list<block>::iterator best = FindBestFreeBlock(nBytes);

	if (best != blocks.end())
	{
		void * address = (pool + best->index);
		block child = { best->index + nBytes,best->size - nBytes,false };
		best->allocated = true;
		best->size = nBytes;
		best++;
		blocks.insert(best, child);

		return address;
	}

	throw OutofMemoryException();
}


void MemoryPool::Free(void * address)
{
	unsigned int index = ((unsigned char *)address - pool);

	list<block>::iterator i = blocks.begin();

	for (i; i != blocks.end(); i++)
	{
		if (i->index == index)
		{
			i->allocated = false;
			if (i != blocks.begin())
			{
				list<block>::iterator prev = i;
				prev--;
				if (!prev->allocated)
				{
					i->index = prev->index;
					i->size += prev->size;
					blocks.erase(prev);
				}
			}

			list<block>::iterator next = i;
			next++;

			if (next != blocks.end() && !next->allocated)
			{
				i->size += next->size;

				blocks.erase(next);
			}
			return;
		}
		else
		{
			throw invalid_argument("Unable to find");
		}
	}
}


void MemoryPool::DebugPrint()
{
	cout << ClassName() << ", " << poolSize << " bytes:" << endl;
	for (block b : blocks)
	{
		cout << "\t" << b.index << ", " << b.size << ", " << (b.allocated ? "allocated" : "free") << endl;
	}
}




FirstFitPool::FirstFitPool(unsigned int poolSize) : MemoryPool(poolSize)
{
}

bool FirstFitPool::usableBlock(block b, unsigned int nBytes)
{
	return !b.allocated && nBytes <= b.size;
}

list<MemoryPool::block>::iterator FirstFitPool::FindBestFreeBlock(unsigned int nBytes)
{
	list<block>::iterator i = blocks.end();
	for (i = blocks.begin(); i != blocks.end() && !usableBlock(*i, nBytes); i++);

	return i;
}



BestFitPool::BestFitPool(unsigned int poolSize) : MemoryPool(poolSize)
{
}

list<MemoryPool::block>::iterator BestFitPool::FindBestFreeBlock(unsigned int nBytes)
{
	list<block>::iterator best = blocks.end();
	for (list<block>::iterator i = blocks.begin(); i != blocks.end(); i++)
	{
		if (!i->allocated && i->size >= nBytes)
		{
			if (best == blocks.end() || i->size < best->size)
			{
				best = i;
			}

		}
	}
	return best;
}


