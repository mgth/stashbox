# Advanced Registration
This section is about Stashbox's further configuration options, including the registration configuration API, the registration of factory delegates, multiple implementations, batch registration, the concept of the [Composition Root](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) and many more.

?> This section won't cover all the available options of the registrations API, but you can find them [here](configuration/registration-configuration).

<!-- panels:start -->

<!-- div:title-panel -->
## Factory Registration

<!-- div:left-panel -->
You have the option to bind a factory delegate to a registration that the container will invoke directly to instantiate your service. 

You can use parameter-less and custom parameterized delegates as a factory. [Here](configuration/registration-configuration?id=factory) is the list of all available options.

You can also get the current dependency resolver as a delegate parameter used to resolve any additional dependencies required for service construction.

<!-- div:right-panel -->

<!-- tabs:start -->

#### **Parameter-less**
```cs
container.Register<ILogger, ConsoleLogger>(options => options
    .WithFactory(() => new ConsoleLogger());

// the container uses the factory for instantiation.
IJob job = container.Resolve<ILogger>();
```

#### **Parameterized**
```cs
container.Register<IJob, DbBackup>(options => options
    .WithFactory<ILogger>(logger => new DbBackup(logger));

// the container uses the factory for instantiation.
IJob job = container.Resolve<IJob>();
```

#### **Resolver parameter**
```cs
container.Register<IJob, DbBackup>(options => options
    .WithFactory(resolver => new DbBackup(resolver.Resolve<ILogger>()));
    
// the container uses the factory for instantiation.
IJob job = container.Resolve<IJob>();
```

<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:left-panel -->
Delegate factories are useful when your service's instantiation is not straight-forward for the container, like when it depends on something that is not available at resolution time. E.g., a connection string.

<!-- div:right-panel -->
```cs
container.Register<IJob, DbBackup>(options => options
    .WithFactory<ILogger>(logger => 
        new DbBackup(Configuration["DbConnectionString"], logger));
```

<!-- panels:end -->

<!-- panels:start -->

<!-- div:left-panel -->
### Factory with Parameter Override
Suppose you'd want to use custom parameters for your service's instantiation rather than captured variables in lambda closures. In that case, you can register a `Func<>` delegate that you can use with parameters at resolution time.

?> This example is about pre-registered factories; however, the container can also implicitly [wrap](advanced/generics?id=func) your service in a `Func<>` without pre-registering.
<!-- div:right-panel -->

<!-- tabs:start -->
#### **Generic API**
```cs
container.RegisterFunc<string, IJob>((connectionString, resolver) => 
    new DbBackup(connectionString, resolver.Resolve<ILogger>()));

Func<string, IJob> backupFactory = container.ResolveFactory<string, IJob>();
IJob dbBackup = backupFactory(Configuration["ConnectionString"]);
```

#### **Runtime type API**
```cs
container.RegisterFunc<string, IJob>((connectionString, resolver) => 
    new DbBackup(connectionString, resolver.Resolve<ILogger>()));

Delegate backupFactory = container.ResolveFactory(typeof(IJob), 
    parameterTypes: new[] { typeof(string) });
IJob dbBackup = backupFactory.DynamicInvoke(Configuration["ConnectionString"]);
```
<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:left-panel -->
### Consider These Before Using the Resolver Parameter Inside a Factory
Delegate factories are a black-box for the container. It doesn't have much control over what's happening inside them, which means when you resolve additional dependencies with the dependency resolver parameter, they could easily bypass the [lifetime](diagnostics/validation?id=lifetime-validation) and [circular dependency](diagnostics/validation?id=circular-dependency) validations. Fortunately, there are options to keep them validated anyway:

- **Parameterized factories instead of resolver**: rather than using the dependency resolver parameter inside the factory, let the container inject the dependencies into the delegate as parameters. With this, the resolution tree's integrity remains stable because no service resolution happens inside the black-box, and each parameter is validated.

- There is a [container configuration option](configuration/container-configuration?id=circular-dependencies-in-delegates) that enables **circular dependency tracking even across delegates that uses the resolver parameter** to resolve dependencies. When this option is enabled, the container generates extra expression nodes into the resolution tree to detect circles, but at the price of a much more complex tree structure and longer dependency walkthrough.

<!-- div:right-panel -->

<!-- tabs:start -->
#### **Parameterized factory**
```cs
interface IEventProcessor { }

class EventProcessor : IEventProcessor
{
    public EventProcessor(ILogger logger, IEventValidator validator)
    { }
}

container.Register<ILogger, ConsoleLogger>();
container.Register<IEventValidator, EventValidator>();

container.Register<IEventProcessor, EventProcessor>(options => options
    // Ilogger and IEventValidator instances are injected
    // by the container at resolution time, so they will be
    // validated against circular and captive dependencies.
    .WithFactory<ILogger, IEventValidator>((logger, validator) => 
        new EventProcessor(logger, validator));

// the container resolves ILogger and IEventValidator first, then
// it passes them to the factory as delegate parameters.
IEventProcessor processor = container.Resolve<IEventProcessor>();
```

#### **Resolver with circle tracking**
```cs
interface IEventProcessor { }

class EventProcessor : IEventProcessor
{
    public EventProcessor(ILogger logger, IEventValidator validator)
    { }
}

// enabling the circular dependency tracking across factory delegates.
using var container = new StashboxContainer(options => 
    options.WithRuntimeCircularDependencyTracking());

container.Register<ILogger, ConsoleLogger>();
container.Register<IEventValidator, EventValidator>();

container.Register<IEventProcessor, EventProcessor>(options => options
    // Ilogger and IEventValidator instances are resolved by the 
    // passed resolver, so they will bypass the circular and captive 
    // dependency validation. However the extra tracker nodes will catch
    // the circular dependencies anyway.
    .WithFactory(resolver => new EventProcessor(
        resolver.Resolve<ILogger>(), resolver.Resolve<IEventValidator>()));

// the container uses the factory to instantiate the processor, and 
// generates the extra circle tracker expression nodes into the tree.
IEventProcessor processor = container.Resolve<IEventProcessor>();
```
<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:title-panel -->
## Multiple Implementations

<!-- div:left-panel -->
As we previously saw in the [Named registration](usage/basics?id=named-registration) topic, Stashbox allows you to have multiple implementations bound to a particular service type. You can use names to distinguish them, but you can also access them by requesting a typed collection using the service type.

?> The returned collection is in the same order as the services were registered.
Also, to request a collection, you can use any interface implemented by an array.

<!-- div:right-panel -->

```cs
container.Register<IJob, DbBackup>();
container.Register<IJob, StorageCleanup>();
container.Register<IJob, ImageProcess>();
```

<!-- tabs:start -->

#### **ResolveAll**
```cs
// jobs contain all three services in registration order.
IEnumerable<IJob> jobs = container.ResolveAll<IJob>();
```

#### **Array**
```cs
// jobs contain all three services in registration order.
IJob[] jobs = container.Resolve<IJob[]>();
```

#### **IEnumerable**
```cs
// jobs contain all three services in registration order.
IEnumerable<IJob> jobs = container.Resolve<IEnumerable<IJob>>();
```

#### **IList**
```cs
// jobs contain all three services in registration order.
IList<IJob> jobs = container.Resolve<IList<IJob>>();
```

#### **ICollection**
```cs
// jobs contain all three services in registration order.
ICollection<IJob> jobs = container.Resolve<ICollection<IJob>>();
```
<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:left-panel -->
When you have multiple implementations registered to a service, a request to the service type without a name will return the **last registered implementation**.

?> Not only names can be used to distinguish registrations, [conditions](usage/service-resolution?id=conditional-resolution) and [named scopes](usage/scopes?id=named-scopes) can also influence the results.
<!-- div:right-panel -->

```cs
container.Register<IJob, DbBackup>();
container.Register<IJob, StorageCleanup>();
container.Register<IJob, ImageProcess>();

// job will be the ImageProcess.
IJob job = container.Resolve<IJob>();
```

<!-- panels:end -->

<!-- panels:start -->

<!-- div:title-panel -->
## Binding to Multiple Services

<!-- div:left-panel -->
When you have a service that implements multiple interfaces, you have the option to bind its registration to all or some of those additional interfaces or base types.

Suppose we have the following class declaration:
```cs
class DbBackup : IJob, IScheduledJob
{ 
    public DbBackup() { }
}
```


<!-- div:right-panel -->

<!-- tabs:start -->
#### **To another type**
```cs
container.Register<IJob, DbBackup>(options => options
    .AsServiceAlso<IScheduledJob>());

IJob job = container.Resolve<IJob>(); // DbBackup
IScheduledJob job = container.Resolve<IScheduledJob>(); // DbBackup
DbBackup job = container.Resolve<DbBackup>(); // error, not found
```

#### **To all implemented types**
```cs
container.Register<DbBackup>(options => options
    .AsImplementedTypes());

IJob job = container.Resolve<IJob>(); // DbBackup
IScheduledJob job = container.Resolve<IScheduledJob>(); // DbBackup
DbBackup job = container.Resolve<DbBackup>(); // DbBackup
```

<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:title-panel -->
## Batch Registration

<!-- div:left-panel -->
You have the option to register multiple services in a single registration operation. 

**Filters (optional):**
First, the container will use the *implementation filter* action to select only those types from the given collection that we want to register. When we have those, the container will execute the *service filter* on their implemented interfaces and base classes to select which service type they should be mapped to.

?> Framework types like `IDisposable` are excluded from being considered as a service type by default.

?> You can use the registration configuration API to configure the individual registrations.

<!-- div:right-panel -->

<!-- tabs:start -->
#### **Default**
This example will register three types to all their implemented interfaces, extended base classes, and to themselves without any filter:
```cs
container.RegisterTypes(new[] 
    { 
        typeof(DbBackup), 
        typeof(ConsoleLogger), 
        typeof(StorageCleanup) 
    });

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
ILogger logger = container.Resolve<ILogger>(); // ConsoleLogger
IJob job = container.Resolve<IJob>(); // StorageCleanup
DbBackup backup = container.Resolve<DbBackup>(); // DbBackup
```

#### **Filters**
In this example, we assume that `DbBackup` and `StorageCleanup` are implementing `IDisposable` besides `IJob` and also extending a `JobBase` abstract class.
```cs
container.RegisterTypes(new[] 
    { typeof(DbBackup), typeof(ConsoleLogger), typeof(StorageCleanup) },
    // implementation filter, only those implementations that implements IDisposable
    impl => typeof(IDisposable).IsAssignableFrom(impl),
    // service filter, register them to base classes only
    (impl, service) => service.IsAbstract && !service.IsInterface);

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 0 items
IEnumerable<JobBase> jobs = container.ResolveAll<JobBase>(); // 2 items
ILogger logger = container.Resolve<ILogger>(); // error, not found
DbBackup backup = container.Resolve<DbBackup>(); // DbBackup
```

#### **Without self**
This example will ignore the mapping of implementation types to themselves completely:
```cs
container.RegisterTypes(new[] 
    { 
        typeof(DbBackup), 
        typeof(ConsoleLogger), 
        typeof(StorageCleanup)
    },
    registerSelf: false);

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
ILogger logger = container.Resolve<ILogger>(); // ConsoleLogger
DbBackup backup = container.Resolve<DbBackup>(); // error, not found
ConsoleLogger logger = container.Resolve<ConsoleLogger>(); // error, not found
```

#### **Registration options**
This example will configure all registrations mapped to `ILogger` as `Singleton`:
```cs
container.RegisterTypes(new[] 
    { 
        typeof(DbBackup), 
        typeof(ConsoleLogger), 
        typeof(StorageCleanup)
    },
    configurator: options => 
    {
        if (options.ServiceType == typeof(ILogger))
            options.WithSingletonLifetime();
    });

ILogger logger = container.Resolve<ILogger>(); // ConsoleLogger
ILogger newLogger = container.Resolve<ILogger>(); // the same ConsoleLogger
IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
```

<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:left-panel -->
Another type of service filter is the `.RegisterTypesAs<T>()` method, which registers only those types that implements the `T` service type.

?> This method also accepts an implementation filter and registration configurator action as the `.RegisterTypes()`.

!> `.RegisterTypesAs<T>()` doesn't create self registrations as it only maps the implementations to the given `T` service type.
<!-- div:right-panel -->

<!-- tabs:start -->
#### **Generic API**
```cs
container.RegisterTypesAs<IJob>(new[] 
    { 
        typeof(DbBackup), 
        typeof(ConsoleLogger), 
        typeof(StorageCleanup) 
    });

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
ILogger logger = container.Resolve<ILogger>(); // error, not found
IJob job = container.Resolve<IJob>(); // StorageCleanup
DbBackup backup = container.Resolve<DbBackup>(); // error, not found
```
#### **Runtime type API**
```cs
container.RegisterTypesAs(typeof(IJob), new[] 
    { 
        typeof(DbBackup), 
        typeof(ConsoleLogger), 
        typeof(StorageCleanup) 
    });

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
ILogger logger = container.Resolve<ILogger>(); // error, not found
IJob job = container.Resolve<IJob>(); // StorageCleanup
DbBackup backup = container.Resolve<DbBackup>(); // error, not found
```
<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:title-panel -->
## Assembly Registration

<!-- div:left-panel -->

The batch registration API's signature *(filters, registration configuration action, self-registration)* is also usable for registering services from given assemblies.

In this example, we assume that the same three services used at the batch registration are in the same assembly.

?> The container also detects and registers open-generic definitions (when applicable) from the supplied type collection. You can read about [open-generics here](advanced/generics?id=open-generics).

<!-- div:right-panel -->

<!-- tabs:start -->
#### **Single assembly**
```cs
container.RegisterAssembly(typeof(DbBackup).Assembly,
    // service filter, register to interfaces only
    serviceTypeSelector: (impl, service) => info.IsInterface,
    registerSelf: false,
    configurator: options => options.WithoutDisposalTracking());

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
IEnumerable<JobBase> jobs = container.ResolveAll<JobBase>(); // 0 items
ILogger logger = container.Resolve<ILogger>(); // ConsoleLogger
DbBackup backup = container.Resolve<DbBackup>(); // error, not found
```

#### **Multiple assemblies**
```cs
container.RegisterAssembly(new[] 
    { 
        typeof(DbBackup).Assembly, 
        typeof(JobFromAnotherAssembly).Assembly 
    },
    // service filter, register to interfaces only
    serviceTypeSelector: (impl, service) => info.IsInterface,
    registerSelf: false,
    configurator: options => options.WithoutDisposalTracking());

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
IEnumerable<JobBase> jobs = container.ResolveAll<JobBase>(); // 0 items
ILogger logger = container.Resolve<ILogger>(); // ConsoleLogger
DbBackup backup = container.Resolve<DbBackup>(); // error, not found
```

#### **Containing type**
```cs
container.RegisterAssemblyContaining<DbBackup>(
    // service filter, register to interfaces only
    serviceTypeSelector: (impl, service) => service.IsInterface,
    registerSelf: false,
    configurator: options => options.WithoutDisposalTracking());

IEnumerable<IJob> jobs = container.ResolveAll<IJob>(); // 2 items
IEnumerable<JobBase> jobs = container.ResolveAll<JobBase>(); // 0 items
ILogger logger = container.Resolve<ILogger>(); // ConsoleLogger
DbBackup backup = container.Resolve<DbBackup>(); // error, not found
```

<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:title-panel -->
## Composition Root

<!-- div:left-panel -->
The [Composition Root](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) is an entry point, where all services required to make a component functional are wired together.

Stashbox provides an `ICompositionRoot` interface that can be used to define an entry point for a given component or even for an entire assembly. 

You can wire up your *composition root* implementation with `ComposeBy<TRoot>()`, or you can let the container find and execute all available *composition roots* within an assembly.

?> Your `ICompositionRoot` implementation also can have dependencies that the container will inject.

<!-- div:right-panel -->

```cs
class ExampleRoot : ICompositionRoot
{
    public ExampleRoot(IDependency rootDependency)
    { }

    public void Compose(IStashboxContainer container)
    {
       container.Register<IServiceA, ServiceA>();
       container.Register<IServiceB, ServiceB>();
    }
}
```

<!-- tabs:start -->
#### **Single**
```cs
// compose a single root.
container.ComposeBy<ExampleRoot>();
```

#### **Assembly**
```cs
// compose every root in the given assembly.
container.ComposeAssembly(typeof(IServiceA).Assembly);
```

#### **Override**
```cs
// compose a single root with dependency override.
container.ComposeBy<ExampleRoot>(new CustomRootDependency());
```

<!-- tabs:end -->

<!-- panels:end -->

<!-- panels:start -->

<!-- div:title-panel -->
## Injection Parameters

<!-- div:left-panel -->
If you have some pre-evaluated dependencies you'd like to inject at resolution time, you can set them as an injection parameter during registration. 

?> Injection parameter names are matched to constructor argument and field/property names.

<!-- div:right-panel -->
```cs
container.Register<IJob, DbBackup>(options => options
    .WithInjectionParameter("logger", new ConsoleLogger())
    .WithInjectionParameter("eventBroadcaster", new MessageBus());

// the injection parameters will be passed to DbBackup's constructor.
IJob backup = container.Resolve<IJob>();
```
<!-- panels:end -->

<!-- panels:start -->

<!-- div:title-panel -->
## Initializer / Finalizer

<!-- div:left-panel -->
The container provides specific extension points that could be used as hooks to react to the instantiated service's lifetime events. 

For this reason, you can specify your own *Initializer* and *Finalizer* delegates. The finalizer is called on the service's [disposal](usage/scopes?id=disposal).
<!-- div:right-panel -->
```cs
container.Register<ILogger, FileLogger>(options => options
    // delegate that called right after instantiation.
    .WithInitializer((logger, resolver) => logger.OpenFile())
    // delegate that called right before the instance's disposal.
    .WithFinalizer(logger => logger.CloseFile()));
```
<!-- panels:end -->