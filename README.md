#Varekai#

Varekai is an experiment. It is a test implementation of the RedLock distributed locking algorithm (see this for more info about RedLock http://redis.io/topics/distlock).

Varekai is written in C# on Mono using Xamarin Studio. I develop and run it on Mac OS X but I cannot see any reasons for it not to run also on the MS .NET Framework.

###Modules###

* __Locker__: the locking library, it implements the locking algorithm
* __Locking Adapter__: a service adapter that blocks the adaptee services until the lock is not acquired
* __Sample Service__: a sample Topshelf service that demonstrate the adapter and simply prints to the output
* __Utility__: a set of usefull functions


###How To Run It###

To run Varekai you need access to a number of instances of Redis server. As all the other quorum based strategies you need an uneven number of Redis servers to guarantee that only one competing service hold the lock at a certain point in time. If you need info on how to configure Redis this is a good place to start http://redis.io/documentation.
To tell to Varekai where to find the Redis servers you have to edit the file RedisNodes.txt of the SampleLockingService project. The version in this repository assumes you have 7 Redis nodes running locally (localhost) listening on the ports from 7001 to 7007

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


###How To Extend It###

The Sample Service is only one possible way to use and test Varekai. I plan to add more and try to explore different use cases for the RedLock algorithm, but I use my spare time for this so I really don't know how it will be able to proceed. In case you want to extend it yourself or simply play with it, all the dependencies of the Locking Adapter are injected using Autofac so it shouldn't be a problem to start from the Sample service and implement your own logic over it.
