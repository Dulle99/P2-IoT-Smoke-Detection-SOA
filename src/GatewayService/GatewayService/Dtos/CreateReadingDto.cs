namespace GatewayService.Dtos
{
    public class CreateReadingDto
    {
        public long Utc { get; set;  }

        public double TemperatureC { get; set; }

        public double HumidityPercent { get; set; }   

        public int ECo2Ppm { get; set; }

        public int TVocPpb { get; set; }

        public double PressureHpa { get; set; }

        public double Pm25 { get; set; }

        public bool FireAlarm { get; set; }
    }
}
