using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using log4net.Repository;

namespace Core.CrossCuttingConcerns.Logging.Log4Net
{
    public class LoggerServiceBase
    {
        private static readonly object ConfigurationLock = new object();
        private static ILoggerRepository? _repository;
        private readonly ILog _log;

        public static void Configure(string connectionString, string configurationPath)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("A database connection string is required.", nameof(connectionString));

            lock (ConfigurationLock)
            {
                if (_repository != null) return;

                var document = new XmlDocument();
                using (var config = File.OpenRead(configurationPath))
                    document.Load(config);

                // Set the connection before log4net activates its database appenders.
                var connectionNodes = document.SelectNodes(
                    "/log4net/appender[@type='log4net.Appender.AdoNetAppender']/connectionString");
                if (connectionNodes is null)
                    throw new InvalidOperationException("The log4net database appender configuration is missing.");

                foreach (XmlElement connection in connectionNodes)
                    connection.SetAttribute("value", connectionString);

                var entryAssembly = Assembly.GetEntryAssembly()
                    ?? throw new InvalidOperationException("The entry assembly is unavailable.");
                var log4NetElement = document["log4net"]
                    ?? throw new InvalidOperationException("The log4net root element is missing.");
                var repository = LogManager.GetRepository(entryAssembly);
                log4net.Config.XmlConfigurator.Configure(repository, log4NetElement);
                _repository = repository;
            }
        }

        public LoggerServiceBase(string name)
        {
            var repository = _repository ?? throw new InvalidOperationException(
                "Configure log4net at application startup before creating loggers.");
            _log = LogManager.GetLogger(repository.Name, name);
        }

        public bool IsInfoEnabled => _log.IsInfoEnabled;
        public bool IsDebugEnabled => _log.IsDebugEnabled;
        public bool IsWarnEnabled => _log.IsWarnEnabled;
        public bool IsFatalEnabled => _log.IsFatalEnabled;
        public bool IsErrorEnabled => _log.IsErrorEnabled;

        public void Info(object logMessage)
        {
            if (IsInfoEnabled)
                _log.Info(logMessage);
        }

        public void Debug(object logMessage)
        {
            if (IsDebugEnabled)
                _log.Debug(logMessage);
        }

        public void Warn(object logMessage)
        {
            if (IsWarnEnabled)
                _log.Warn(logMessage);
        }

        public void Fatal(object logMessage)
        {
            if (IsFatalEnabled)
                _log.Fatal(logMessage);
        }

        public void Error(object logMessage)
        {
            if (IsErrorEnabled)
                _log.Error(logMessage);
        }


    }
}
