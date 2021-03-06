#include <iostream>
#include "Pool.h"
#include "Queue.h"

using namespace std;



void TestFirstFitPool()
{
	FirstFitPool pool(100);
	pool.DebugPrint();
	void * block1 = pool.Allocate(9);
	pool.DebugPrint();
	void * block2 = pool.Allocate(1);
	pool.DebugPrint();
	void * block3 = pool.Allocate(6);
	pool.DebugPrint();
	void * block4 = pool.Allocate(1);
	pool.DebugPrint();
	pool.Free(block3);
	pool.DebugPrint();
	pool.Free(block1);
	pool.DebugPrint();
	void * block5 = pool.Allocate(5);
	pool.DebugPrint();
	pool.Free(block2);
	pool.DebugPrint();
	pool.Free(block3);
	pool.DebugPrint();


	try
	{
		FirstFitPool pool2(100);
		pool.DebugPrint();
		void * block1 = pool2.Allocate(101);
		pool.DebugPrint();
		pool2.Free(block1);
		pool.DebugPrint();
	}
	catch (OutofMemoryException)
	{
		cout << "Out of Memory!" << endl;
	}

	try
	{
		FirstFitPool pool3(100);
		pool3.DebugPrint();
		void * block1 = pool3.Allocate(50);
		pool3.DebugPrint();
		void * block2 = pool3.Allocate(60);
		pool3.DebugPrint();
	}
	catch (OutofMemoryException)
	{
		cout << "Out of Memory!" << endl;
	}
}

void TestBestFitPool()
{
	BestFitPool pool(100);
	pool.DebugPrint();
	void * block1 = pool.Allocate(9);
	pool.DebugPrint();
	void * block2 = pool.Allocate(1);
	pool.DebugPrint();
	void * block3 = pool.Allocate(6);
	pool.DebugPrint();
	void * block4 = pool.Allocate(1);
	pool.DebugPrint();
	pool.Free(block3);
	pool.DebugPrint();
	pool.Free(block1);
	pool.DebugPrint();
	void * block5 = pool.Allocate(5);
	pool.DebugPrint();
	pool.Free(block2);
	pool.DebugPrint();
	pool.Free(block3);
	pool.DebugPrint();
}

void TestStringQueue()
{
	FirstFitPool pool(100);
	StringQueue q(&pool);
	pool.DebugPrint();
	q.Insert((char*)"foo");
	pool.DebugPrint();
	q.Insert((char*)"bar");
	pool.DebugPrint();
	char * s1 = q.Peek();
	cout << "s1 = " << s1 << endl;
	q.Remove();
	pool.DebugPrint();
	char * s2 = q.Peek();
	cout << "s2 = " << s2 << endl;
	q.Remove();
	pool.DebugPrint();
}


int main()
{
	TestFirstFitPool();
	TestBestFitPool();


	return 0;
}