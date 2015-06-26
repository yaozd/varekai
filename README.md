# Varekai
Varekai is an experiment, it's a test implementation of the RedLock distributed locking algorithm (see this for more http://redis.io/topics/distlock).

It is composed of the following Xamarin Studio C# projects
- the locking library, implementing the locking strategy
- a service adapter that contains the engine to block the services on the lock's acquisition
- a sample Topshelf service using the service adapter to start the execution after the lock's acquisition
- some utility andtest projects
