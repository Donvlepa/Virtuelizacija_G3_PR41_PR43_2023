using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class WeatherSample
    {
        double t;
        double pressure;
        double tpot;
        double tdew;
        double vPmax;
        double vPdef;
        double vPact;
        DateTime date;

        public WeatherSample():this(0, 0, 0, 0, 0, 0, 0, new DateTime()) { }

        public WeatherSample(double t, double pressure, double tpot, double tdew, double vPmax, double vPdef, double vPact, DateTime date)
        {
            this.t = t;
            this.pressure = pressure;
            this.tpot = tpot;
            this.tdew = tdew;
            this.vPmax = vPmax;
            this.vPdef = vPdef;
            this.vPact = vPact;
            this.date = date;
        }

        [DataMember]
        public double T { get => t; set => t = value; }

        [DataMember]
        public double Pressure { get => pressure; set => pressure = value; }

        [DataMember]
        public double Tpot { get => tpot; set => tpot = value; }

        [DataMember]
        public double Tdew {  get => tdew; set => tdew = value; }

        [DataMember]
        public double VPmax { get => vPmax; set => vPmax = value; }

        [DataMember]
        public double VPdef { get => vPdef; set => vPdef = value; }

        [DataMember]
        public double VPact { get => vPact; set => vPact = value; }

        [DataMember]
        public DateTime Date { get => date; set => date = value; }

    }
}
