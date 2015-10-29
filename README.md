#Varekai#

Varekai is an experiment. It's a test implementation of the RedLock distributed locking algorithm (see <a href="http://redis.io/topics/distlock" target="_blank">this</a> for details about <a href="http://redis.io/topics/distlock" target="_blank">RedLock</a>).

Varekai is written in C# on Mono using Xamarin Studio. I develop and run it on Mac OS X but I cannot see any reasons for it not to run also on the MS .NET Framework.

###Modules###

* __Locker__: the locking library, it implements the locking algorithm
* __Sample Services__: sample Topshelf services that demonstrate the adapter in some use cases
* __Infrastructure Helpers__: some helper functions to speed up the services' creation
* __Utility__: a set of useful functions


###How To Run It###

To run Varekai you need access to a number of instances of Redis server. The algorithm is quorum based so, in case of compeeting clients, the lock will be considered held if the write succeedes at least on the half plus one of the Redis servers. This makes possible to have any number (even or uneven) of nodes, even though doesn't make a lot of sense having less than three. If you need info on how to configure Redis the official <a href="http://redis.io/documentation" target="_blank">documentation</a> is a good place to start. Also you might find the tips <a href="https://barambani.wordpress.com/2015/04/02/redis-cluster" target="_blank">here</a> useful if you are running Redis on Mac OS X.

To tell to Varekai where it will find the Redis servers you have to edit the file RedisNodes.txt of the HelloWorldService project. The version in this repository assumes you have 7 Redis nodes running locally (localhost) and listening on the ports from 7001 to 7007

```
[
	{
		address: 'localhost',
		port: 7001
	},
	{
		address: 'localhost',
		port: 7002
	},
	{
		address: 'localhost',
		port: 7003
	},
	{
		address: 'localhost',
		port: 7004
	},
	{
		address: 'localhost',
		port: 7005
	},
	{
		address: 'localhost',
		port: 7006
	},
	{
		address: 'localhost',
		port: 7007
	}
]
```


###How To Use It###

The sample services are only some possible ways to use and experiment Varekai. I plan to add more of them and try to explore different use cases for the RedLock algorithm, but I use my spare time to do it, so I really don't know how much it will be able to proceed. In case you want to play with it, all the dependencies of the Locking Adapter are injected using Autofac so it shouldn't be a problem to start from the Hello World service and implement your own logic over it.
