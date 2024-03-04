using System;

namespace Bynder.Content.SitecoreConnector.Managers.Interfaces
{
    public interface ILogger
    {
        void Info(string message, object sender);

        void Warn(string message, object sender);

        void Error(string message, Exception exception);
    }

    public class NullLogger : ILogger
    {
        public void Info(string message, object sender)
        {
            Console.WriteLine("INFO: " + sender.GetType().FullName + " : " + message);
        }

        public void Warn(string message, object sender)
        {
            Console.WriteLine("WARN: " + sender.GetType().FullName + " : " + message);
        }

        public void Error(string message, Exception exception)
        {
            Console.WriteLine("ERROR: " + message);
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.StackTrace);
        }
    }
}
