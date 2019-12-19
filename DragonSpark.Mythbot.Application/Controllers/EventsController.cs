using DragonSpark.Model.Selection;
using DragonSpark.Model.Selection.Alterations;
using DragonSpark.Model.Sequences;
using DragonSpark.Mythbot.Environment;
using DragonSpark.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DragonSpark.Mythbot.Application.Controllers
{
	[ApiController, Route("[controller]")]
	public class EventsController : ControllerBase
	{
		readonly IWeatherForecastService   _weather;
		readonly ILogger<EventsController> _logger;

		public EventsController(IWeatherForecastService weather, ILogger<EventsController> logger)
		{
			_weather = weather;
			_logger  = logger;
		}

		public Task Post(EventMessage message)
		{
			return Task.CompletedTask;
		}

		[HttpGet]
		public async Task<WeatherForecast[]> Get()
		{
			var result = await _weather.GetForecast(DateTime.Now);
			_logger.Log(LogLevel.Debug, "Served {Number} forecasts.", result.Length.ToString());
			return result;
		}
	}

	[ModelBinder(BinderType = typeof(EventMessageBinder))]
	public sealed class EventMessage
	{
		public EventMessage(EventHeader header, string payload, bool isAuthenticated)
		{
			Header          = header;
			Payload         = payload;
			IsAuthenticated = isAuthenticated;
		}

		public EventHeader Header { get; }

		public string Payload { get; }

		public bool IsAuthenticated { get; }
	}

	/*// [ModelBinder(BinderType = typeof(EventBinder))]
	public sealed class Event<T> where T : ActivityPayload
	{
		public Event(EventHeader header, T payload)
		{
			Header  = header;
			Payload = payload;
		}

		public EventHeader Header { get; }

		public T Payload { get; }
	}*/

	sealed class Hasher : IAlteration<string>
	{
		readonly Array<byte> _token;

		public Hasher(RelayerSettings settings)
			: this(new Array<byte>(Encoding.UTF8.GetBytes(settings.Token))) {}

		public Hasher(Array<byte> token) => _token = token;

		public string Get(string parameter)
		{
			using (var sha = new HMACSHA1(_token))
			{
				var hash   = sha.ComputeHash(Encoding.UTF8.GetBytes(parameter));
				var result = $"sha1={BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant()}";
				return result;
			}
		}
	}

	public sealed class EventHeader
	{
		public EventHeader(string @event, string delivery)
		{
			Event    = @event;
			Delivery = delivery;
		}

		public string Event { get; }

		public string Delivery { get; }
	}

	readonly struct EventHeaderContext
	{
		public EventHeaderContext(string @event, string delivery, string signature)
		{
			Event     = @event;
			Delivery  = delivery;
			Signature = signature;
		}

		public string Event { get; }

		public string Delivery { get; }

		public string Signature { get; }
	}

	sealed class EventHeaderContexts : ISelect<IDictionary<string, StringValues>, EventHeaderContext>
	{
		public static EventHeaderContexts Default { get; } = new EventHeaderContexts();

		EventHeaderContexts() {}

		public EventHeaderContext Get(IDictionary<string, StringValues> parameter)
			=> new EventHeaderContext(parameter["x-github-event"], parameter["x-gitHub-delivery"],
			                          parameter["X-hub-signature"]);
	}

	/*sealed class Payload<T> : ISelect<HttpRequest, Event<T>> where T : ActivityPayload
	{
		readonly Hasher              _hasher;
		readonly EventHeaderContexts _contexts;
		readonly IJsonSerializer     _serializer;

		public Payload(Hasher hasher, IJsonSerializer serializer)
			: this(hasher, EventHeaderContexts.Default, serializer) {}

		public Payload(Hasher hasher, EventHeaderContexts contexts, IJsonSerializer serializer)
		{
			_hasher     = hasher;
			_contexts   = contexts;
			_serializer = serializer;
		}

		public Event<T> Get(HttpRequest parameter)
		{
			using (var sr = new StreamReader(parameter.Body))
			{
				var content = sr.ReadToEnd();
				var context = _contexts.Get(parameter.Headers);
				var header = new EventHeader(context.Event, context.Delivery,
				                             _hasher.Get(content) == context.Signature);
				var payload = _serializer.Deserialize<T>(content);
				var result  = new Event<T>(header, payload);
				return result;
			}
		}
	}*/

	sealed class EventMessages : IOperation<HttpRequest, EventMessage>
	{
		readonly Hasher              _hasher;
		readonly EventHeaderContexts _contexts;

		public EventMessages(Hasher hasher) : this(hasher, EventHeaderContexts.Default) {}

		public EventMessages(Hasher hasher, EventHeaderContexts contexts)
		{
			_hasher   = hasher;
			_contexts = contexts;
		}

		public async ValueTask<EventMessage> Get(HttpRequest parameter)
		{
			using (var reader = new StreamReader(parameter.Body))
			{
				var content = await reader.ReadToEndAsync();
				var context = _contexts.Get(parameter.Headers);
				var header  = new EventHeader(context.Event, context.Delivery);
				var result  = new EventMessage(header, content, _hasher.Get(content) == context.Signature);
				return result;
			}
		}
	}

	/*sealed class EventBinder<T> : ISelect<ModelBindingContext, object> where T : ActivityPayload
	{
		readonly Payload<T> _payload;

		public EventBinder(Payload<T> payload) => _payload = payload;

		public object Get(ModelBindingContext parameter) => _payload.Get(parameter.HttpContext.Request);
	}*/

	sealed class EventMessageBinder : IModelBinder
	{
		readonly Func<HttpRequest, ValueTask<EventMessage>> _messages;

		public EventMessageBinder(EventMessages messages) : this(messages.Get) {}

		EventMessageBinder(Func<HttpRequest, ValueTask<EventMessage>> messages) => _messages = messages;

		public async Task BindModelAsync(ModelBindingContext bindingContext)
		{
			var message = await _messages(bindingContext.HttpContext.Request);

			bindingContext.Result = message != null
				                        ? ModelBindingResult.Success(message)
				                        : ModelBindingResult.Failed();
		}
	}

	/*sealed class EventBinder : IModelBinder
	{
		readonly IServiceProvider           _provider;
		readonly ISelect<Array<Type>, Type> _definition;

		public EventBinder(IServiceProvider provider) : this(provider, new MakeGenericType(typeof(EventBinder<>))) {}

		public EventBinder(IServiceProvider provider, ISelect<Array<Type>, Type> definition)
		{
			_provider   = provider;
			_definition = definition;
		}

		public Task BindModelAsync(ModelBindingContext bindingContext)
		{
			var payloadType = bindingContext.ModelType.GetGenericTypeDefinition().GetGenericArguments().Single();
			var requestType = _definition.Get(payloadType);
			var binder      = _provider.GetRequiredService(requestType).To<ISelect<ModelBindingContext, object>>();
			var @event      = binder.Get(bindingContext);

			bindingContext.Result = @event != null
				                        ? ModelBindingResult.Success(@event)
				                        : ModelBindingResult.Failed();

			return Task.CompletedTask;
		}
	}*/
}