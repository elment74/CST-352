#include "Queue.h"
#include <string.h>

StringQueue::StringQueue(MemoryPool *pool) : pool(pool)
{

}

void StringQueue::Insert(char *s)
{
	try
	{
		unsigned int blockSize = (unsigned int)strlen(s) + 1;
		char * block = (char*)pool->Allocate(blockSize);
		strcpy_s(block, blockSize, s);

		Queue.push(block);
	}
	catch (OutofMemoryException)
	{
		throw FullException();
	}
}

char * StringQueue::Peek()
{
	char *s = Queue.front();
	return s;
}

void StringQueue::Remove()
{
	char *s = Queue.front();
	pool->Free(s);
	Queue.pop();
}
