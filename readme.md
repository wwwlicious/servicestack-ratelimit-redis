# ServiceStack.RateLimit.Redis
[![Build status](https://ci.appveyor.com/api/projects/status/3m3pl4fqwjnoyp48/branch/master?svg=true)](https://ci.appveyor.com/project/wwwlicious/servicestack-ratelimit-redis/branch/master)
[![NuGet version](https://badge.fury.io/nu/servicestack.ratelimit.redis.svg)](https://badge.fury.io/nu/servicestack.ratelimit.redis)

A rate limiting plugin for [ServiceStack](https://servicestack.net/) that uses [Redis](http://redis.io/) for calculating and persisting request counts. 

## Requirements

An accessible running Redis instance. 

The plugin needs to be passed an IRedisClientsManager instance to work.

## Quick Start

Install the package [https://www.nuget.org/packages/ServiceStack.RateLimit.Redis](https://www.nuget.org/packages/ServiceStack.RateLimit.Redis/)
```bash
PM> Install-Package ServiceStack.RateLimit.Redis
```

Add the following to your `AppHost.Configure` method to register the plugin:

```csharp
public override void Configure(Container container)
{
    // Register Redis client manager using locally running Redis instance
	Container.Register<IRedisClientsManager>(new BasicRedisClientManager("127.0.0.1:6379"));
	
	// Register plugin. Every service is now rate limited!
	Plugins.Add(new RateLimitFeature(Container.Resolve<IRedisClientsManager>()));
}
```

There is a baked-in default limit for each DTO type of: 10 requests per minute, 30 request per hour.

To override this add an AppSetting with key *lmt:default* to the App.Config/Web.Config of project running AppHost will provide limit values.

```
<!-- default of 10 per second, 50 per minute, 200 per hour-->
<add key="lmt:default" value="{Limits:[{Limit:10,Seconds:1},{Limit:50,Seconds:60},{Limit:200,Seconds:3600}]}"/>
```

The lookup keys for more granular control are specified below.

### Demo

The included DemoService is a self hosted AppHost listening on port 8090 with Resource and User limits.

Basic authentication is used for identifying users. There are 3 users: Cheetara, Panthro and Tygra. All users have a password of: "password" (without the quotes!).

The "Postman Samples" folder contains a sample [Postman](https://www.getpostman.com/) collection containing a few calls including authorisation setup.


## Overview
The plugin registers a [global request filter](https://github.com/ServiceStack/ServiceStack/wiki/Request-and-response-filters#global-request-filters). Every time a request is received a check is made using a [Redis LUA script](http://redis.io/commands/eval). If the specified limits have not been hit then the request is processed as expected. However, if the limit has been reached then a [429](https://tools.ietf.org/html/rfc6585#page-3) "Too Many Requests" response is generated and processing of the request is halted.

Two possible headers are returned from any endpoint that is protecte: x-ratelimit-request and x-ratelimit-user. They will show the seconds duration, the limit and how many remaining calls are available per request, or user respectively.

### Rate Limits

At a high level, rate limits can be set at either **User** or **Resource** level (by default a resource in this instance is the DTO type name). Limits are fetched from [IAppSettings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings) and can be set at the following levels, in order of precedence:

* User for resource - User 123 can make X requests to a specific resource (e.g. /api/products). (config key: "lmt:{resourceName}:{userId}")
* User - User 123 can make X total requests for specific time period(s). (config key: "lmt:usr:{userId}")
* User fallback - Fallback total request limit for users without specific defaults. (config key: "lmt:usr:default")
* Resource - Each user can make X requests to a resource (e.g. /api/products) for specific time period(s). (config key: "lmt:{resourceName}".)
* Resource fallback - Fallback limit for requests individual users can make to a resource. (config key: "lmt:default")

User limits AND resource limits will be calculated at the same time (if present). User limits are calculated first. If a limit is hit subsequent wider limits are not incremented (e.g. if limit per second is hit, limit per minute would not be counted).

#### Limit Representation
All limits are per second and are stored as a LimitGroup object serialised to JSV. For example, the following shows a limit of 5 requests per second, 15 per minute (60s) and 100 per hour (3600s):
```xml
{Limits:[{Limit:5,Seconds:1},{Limit:15,Seconds:60},{Limit:100,Seconds:3600}]}
```

#### LUA Script
A LUA script is used for doing the heavy lifting and keeping track of limit totals. To save bandwith on calls to Redis the [EVALSHA](http://redis.io/commands/evalsha) command is used to call a LUA script which has previously been [loaded](http://redis.io/commands/script-load).

The default implementation of ILimitProvider (see below) will check IAppSettings for a value with key "script:ratelimit". This value will be the SHA1 of the script to use. Using this method means that the script can be managed external to the plugin.

If an AppSetting is not found with the specified key then the RateLimitHash.lua script is loaded, the SHA1 is stored and used for subsequent requests.

**Note:** The RateLimitHash.lua script does not currently use a sliding expiry, instead is resets every X seconds. E.g. if the limit is for 50 requests in 3600 seconds (1 hour) then 50 requests could be made at 10:59 and then 50 request can be made at 11:01. This is something that may be looked at in the future.

### Extensibility
There are a few extension point that can be set when adding the plugin:

* CorrelationIdExtractor - This is a delegate function that customises how an individual request is identified. By default it uses the value of HTTP Header with name specified by CorrelationIdHeader property. **Note:** This is primarily required for when a ServiceStack service calls subsequent ServiceStack services that all use this plugin as it will avoid user totals being incremented multiple times for the same request.
* CorrelationIdHeader - The name of the header used for extracting correlation Id from request (if using default method). Default: x-mac-requestid.
* StatusDescription - the status description returned when limit is breached. Default "Too many requests".
* LimitStatusCode - the status code returned when limit is breached. Default 429.
* KeyGenerator - an implementation of IKeyGenerator for generating config lookup key(s) for request. Defaults outlined above.
* LimitProvider - an implementation of ILimitProvider for getting RateLimits for current request. Default uses IKeyGenerator keys to lookup IAppSettings.

These are all properties of the RateLimitFeature class and can be set when instantiating the plugin
```csharp
Plugins.Add(new RateLimitFeature(Container.Resolve<IRedisClientsManager>())
{
    CorrelationIdExtractor = req => req.RawUrl,
    KeyGenerator = new MyKeyGenerator()
});
```

### Caveats

Since user limits are available a user **must** be authenticated or the request will return a 401: Forbidden response. This is a default behaviour and can be changed by overriding the `LimitKeyGenerator.GetConsumerId(request)` method.

The script needs to be updated to take a list of all Redis Keys that will be operated on. This is documented in the [Redis EVAL documentation](http://redis.io/commands/EVAL) and is particularly relevant if running a Redis Cluster.

### Extras

* [ServiceStack.Request.Correlation](https://github.com/MacLeanElectrical/servicestack-request-correlation) - Designed to work seamlessly with this plugin, it will ensure that service to service calls will not increment api usage stats
* [ServiceStack.Configuration.Consul](https://github.com/MacLeanElectrical/servicestack-configuration-consul) -
This plugin works well with a shared configuration model where rate limits can be centrally managed globally or across multiple instances of your servicestack instances. The rate limiting scripts can also be updated centrally to make adjustments at runtime.

## Attributions

* http://www.corytaylor.ca/api-throttling-with-servicestack/ by Cory Taylor
