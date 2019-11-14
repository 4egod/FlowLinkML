using Microsoft.ML.Data;
using System;

namespace FlowLinkML.Models
{
    public class Archive 
    {
        [NoColumn]
        public Guid Id { get; set; }

        [NoColumn]
        public int DeviceId { get; set; }

        [NoColumn]
        public DateTime Timestamp { get; set; }

        [ColumnName("Timestamp"), LoadColumn(0)]
        public string TimestampAsString => Timestamp.ToString();

        [ColumnName("DifferentialPressure"), LoadColumn(1)]
        public float DifferentialPressure { get; set; }


        [ColumnName("Pressure"), LoadColumn(2)]
        public float Pressure { get; set; }


        [ColumnName("Temperature"), LoadColumn(3)]
        public float Temperature { get; set; }


        [ColumnName("Volume"), LoadColumn(4)]
        public float Volume { get; set; }


        [ColumnName("FlowTime"), LoadColumn(5)]
        public float FlowTime { get; set; }
    }
}
