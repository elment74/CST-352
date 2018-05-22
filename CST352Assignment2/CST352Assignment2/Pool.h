#pragma once

#include <list>
using namespace std;

class OutofMemoryException
{

};

class MemoryPool
{
protected:
	unsigned int poolSize;
	unsigned char * pool;
	struct block
	{
		unsigned int index;
		unsigned int size;
		bool allocated;
	};
	list<block> blocks;


	virtual list<block>::iterator FindBestFreeBlock(unsigned int nBytes) = 0;
	virtual char * ClassName() = 0;

public:
	MemoryPool(unsigned int poolSize);
	void * Allocate(unsigned int nBytes);
	void Free(void * block);
	void DebugPrint();
};

class FirstFitPool : public MemoryPool
{
private:

	bool usableBlock(block b, unsigned int nBytes);

public:
	FirstFitPool(unsigned int poolSize);

protected:
	virtual list<block>::iterator FindBestFreeBlock(unsigned int nBytes);
	virtual char * ClassName() { return (char*)"FirstFitPool"; }
};

class BestFitPool : public MemoryPool
{
public:
	BestFitPool(unsigned int poolSize);

protected:
	virtual list<block>::iterator FindBestFreeBlock(unsigned int nBytes);
	virtual char * ClassName() { return (char*)"BestFitPool"; }
};