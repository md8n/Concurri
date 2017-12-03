using System;
using System.Threading;
using DotNetify;

namespace Concurri.Web
{
    public class HelloWorld : BaseVM
    {
        private Timer _timer;
        public string Greetings => "Concurri.Web!";
        public DateTime ServerTime => DateTime.Now;

        public HelloWorld()
        {
            _timer = new Timer(state =>
            {
                Changed(nameof(ServerTime));
                PushUpdates();
            }, null, 0, 1000);
        }

        public override void Dispose() => _timer.Dispose();
    }
}