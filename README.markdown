# Service Wrapper

This is a mono-compatible utility for .NET console apps, it lets you run them as services and recompile and restart them when a new build is detected.  You can also add new services on the fly.

##  Prerequisites

1.  You'll need [MongoDB](http://mongodb.org) or similar to hold the service information.  You can just set up a freebie database at [MongoHQ](http://mongohq.com) if necessary.  Inside your database lives a single collection with one of these documents for each service:  ```{ name: "servicename", path: "/path/to/the/project", restart: false }```

2.  You'll need [Mono](http://mono-project.org) although there's no reason it won't work on Windows as well.

3.  I created a special account that has read access to my private repositories so this service can just do a 'git pull', but it would be trivial to tie this to build hooks or whatever API.

## How to use

1.  git clone https://github.com/benlowry/mono-service-wrapper
2.  cd mono-service-wrapper
3.  put the right mongodb connection in the app.config
4.  xbuild
5.  launch with mono-service ./service-wrapper/bin/Debug/ServiceWrapper.exe

## What it doesn't do

This is very basic, it doesn't gracefully stop your services, it doesn't let you stop, remove or schedule services which would all be pretty cool features.

### License

Copyright [Playtomic Inc](https://playtomic.com), 2012.  Licensed under the MIT license.  Certain portions may come from 3rd parties and carry their own licensing terms and are referenced where applicable.