﻿using ClassTranscribeDatabase;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TaskEngine.Tasks;
using System.Collections.Specialized;
using Quartz.Impl;
using Quartz;
using TaskEngine.Grpc;
using TaskEngine.MSTranscription;
using Microsoft.Extensions.Options;
using System.Linq;

namespace TaskEngine
{
    public static class TaskEngineGlobals
    {
        public static KeyProvider KeyProvider { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            //setup our DI
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddOptions()
                .Configure<AppSettings>(CTDbContext.GetConfigurations())
                .AddSingleton<RabbitMQ>()
                .AddSingleton<DownloadPlaylistInfoTask>()
                .AddSingleton<DownloadMediaTask>()
                .AddSingleton<ConvertVideoToWavTask>()
                .AddSingleton<TranscriptionTask>()
                .AddSingleton<WakeDownloaderTask>()
                .AddSingleton<RpcClient>()
                .AddSingleton<MSTranscriptionService>()
                .BuildServiceProvider();

            //configure console logging

            serviceProvider
                .GetService<ILoggerFactory>()
                .AddConsole(LogLevel.Debug);

            Globals.appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;
            TaskEngineGlobals.KeyProvider = new KeyProvider(Globals.appSettings);

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            RabbitMQ rabbitMQ = serviceProvider.GetService<RabbitMQ>();
            CTDbContext context = CTDbContext.CreateDbContext();
            Seeder.Seed(context);
            logger.LogDebug("Starting application");
            serviceProvider.GetService<DownloadPlaylistInfoTask>().Consume();
            serviceProvider.GetService<DownloadMediaTask>().Consume();
            serviceProvider.GetService<ConvertVideoToWavTask>().Consume();
            serviceProvider.GetService<TranscriptionTask>().Consume();
            serviceProvider.GetService<WakeDownloaderTask>().Consume();
            RunProgramRunExample(rabbitMQ).GetAwaiter().GetResult();

            TranscriptionTask transcriptionTask = serviceProvider.GetService<TranscriptionTask>();
            context.Videos.Where(v => v.TranscriptionStatus != "NoError").ToList().ForEach(v => transcriptionTask.Publish(v));

            logger.LogDebug("All done!");

            Console.WriteLine("Press any key to close the application");
            
             while (true) {
                Console.Read();
            };
        }

        private static async Task RunProgramRunExample(RabbitMQ rabbitMQ)
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();

                // and start it off
                await scheduler.Start();

                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<DownloadPlaylistInfoTask>()
                    .WithIdentity("job1", "group1")
                    .Build();

                job.JobDataMap.Put("rabbitMQ", rabbitMQ);

                // Trigger the job to run now, and then repeat every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1", "group1")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        .WithIntervalInHours(6)
                        .RepeatForever())
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);

                // some sleep to show what's happening
                await Task.Delay(TimeSpan.FromSeconds(60));

                // and last shut down the scheduler when you are ready to close your program
                await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }
        }
    }
}
