Varekai
======================

Varekai is an experiment. It is a test implementation of the RedLock distributed locking algorithm (see this for more info on RedLock http://redis.io/topics/distlock).

Varekai is written in C# on Mono using Xamarin Studio. I develop and run it on Mac OS X but I cannot see any reasons for it not to run also on the MS .NET Framework.

Modules
---------------

* __Locker__: the locking library, it implements the locking algorithm
* __Locking Adapter__: a service adapter that blocks the adaptee services until the lock is not acquired
* __Sample Service__: a sample Topshelf service that demostrate the adapter and simply prints on the output when it can run
* __Utility__: a set of usefull functions
