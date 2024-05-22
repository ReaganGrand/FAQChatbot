using FAQChatbot;
using FAQChatbot.Bots;
using FAQChatbot.Dialogs;
using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;
//using Serilog.Templates;

Log.Logger = new LoggerConfiguration()
    //.WriteTo.Console()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    //.WriteTo.Async(wt => wt.Console())
    //.WriteTo.Console(theme: AnsiConsoleTheme.Code,outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try{

    Log.Information("Starting up!");

    var builder = WebApplication.CreateBuilder(args);
    //builder.Services.AddSerilog();
    builder.Services.AddSerilog((service, lc) => lc
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(service)
                    .Enrich.FromLogContext()
                    //.WriteTo.Console()
                    //.WriteTo.Console(new CustomJsonFormatter())
                    //.WriteTo.Console(new RenderedCompactJsonFormatter())
                    //.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {ClassName}.{MethodName} - {Message:lj}{NewLine}{Exception}")
                    //.WriteTo.Debug()                
                    //new ExpressionTemplate(
                    // Include trace and span ids when present.
                    //"[{@t:HH:mm:ss} {@l:u3}{#if @tr is not null} ({substring(@tr,0,4)}:{substring(@sp,0,4)}){#end}] {@m}\n{@x}",
                    //theme: TemplateTheme.Code)))
                    );

    ConfigureState(builder.Services);   

    await using var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        //app.UseHsts();
    }

    //app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    //app.UseSerilogRequestLogging(opts  => opts.EnrichDiagnosticContext = LogEnricher.EnrichFromRequest);
    app.UseSerilogRequestLogging();

    app.UseAuthorization();

    app.MapPost("api/messages", (IBotFrameworkHttpAdapter adapter, IBot bot, HttpContext context) =>
    adapter.ProcessAsync(context.Request, context.Response, bot));
    //app.MapControllers();
    /*.MapControllerRoute(
        name: "default",
        pattern: "{controller=Bot}/{action=Index}/{id?}");
    */

    await app.RunAsync();

    Log.Information("Stopped cleanly");
    return 0;

}
catch(Exception ex){
     Log.Fatal("An unhandled exception with {message} occurred during bootstrapping",ex.Message);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}


void ConfigureState(IServiceCollection services)
{  
    try{

        services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
        });

        // Create the Bot Framework Authentication to be used with the Bot Adapter.
        services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
        // Create the Bot Framework Adapter with error handling enabled.
        services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
        // Add services to the container.
        //services.AddControllersWithViews();

        // Create the bot services(QnA) as a singleton.
        services.AddSingleton<IBotServices, BotServices>();

        // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
        services.AddSingleton<IStorage, MemoryStorage>();

        // Create the User state. (Used in this bot's Dialog implementation.)
        services.AddSingleton<UserState>();

        // Create the Conversation state. (Used by the Dialog system itself.)
        services.AddSingleton<ConversationState>();

        // Create an instance of the state service
        services.AddSingleton<StateService>();
                
        services.AddHttpClient();

        // Register the MainMenuDialog.
        services.AddSingleton<MainMenuDialog>();
        // Register the ApplicationDialog.
        services.AddSingleton<ApplicationDialog>();
        // Register the ContactUs.
        services.AddSingleton<ContactUsDialog>();
        //Register the QuestionDialog.
        services.AddSingleton<QuestionDialog>();
        //Register the ApplicationMenuDialog.
        services.AddSingleton<ApplicationMenuDialog>();
        //Register the TicketDialog.
        services.AddSingleton<TicketDialog>();
        //Register the NoMatchDialog.
        services.AddSingleton<NoMatchDialog>();
        //Register the CQADialog.
        services.AddSingleton<CQADialog>();               

        // The MainDialog that will be run by the bot.
        services.AddSingleton<MainDialog>();

        // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
        services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();
        //ComponentRegistration.Add(new DialogsComponentRegistration());
        //new BotComponent().ConfigureServices(services, configuration);
    }
    catch(Exception ex)
    {
        Log.Error("An unhandled exception occurred {message}",ex.Message);
    }    
}

