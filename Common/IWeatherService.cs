using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface IWeatherService
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse StartSession(SessionMeta meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse PushSample(WeatherSample sample);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        ServiceResponse EndSession();
    }
}
