

#include <queue>
#include "Pool.h"

using namespace std;

class FullException
{

};

class StringQueue
{
	public:
		StringQueue(MemoryPool *pool);
		void Insert(char *s);
		char *Peek();
		void Remove();

	private:
		MemoryPool *pool;
		queue<char*> Queue;
};