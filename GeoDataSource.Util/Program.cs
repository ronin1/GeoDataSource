using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using UpdateStep = GeoDataSource.DataManager.UpdateStep;

namespace GeoDataSource.Util
{
    sealed class Program
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        static Program()
        {
            log4net.Config.XmlConfigurator.Configure();
            _logger.Debug("Initialization");            
        }

        static void Main(params string[] args)
        {
            try {
                var p = new Program(args);
                p.Run();
            }
            catch(Exception ex)
            {
                _logger.Fatal("Main: " + string.Join(" ", args), ex);
            }
        }

        readonly UpdateStep _steps = UpdateStep.None;

        private Program(string[] args)
        {
            if (args == null || args.Length == 0)
                _steps = UpdateStep.All;
            else
            {
                foreach (string s in args)
                {
                    UpdateStep us;
                    if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse(s, true, out us))
                        _steps |= us;
                }
            }
            _logger.InfoFormat("Start: {0}", _steps);
        }

        public void Run()
        {
            DateTime started = DateTime.UtcNow;

            DataManager.Instance.Update(_steps).Wait();

            _logger.InfoFormat("Finished: {0}", DateTime.UtcNow - started);
        }
        
    }
}
