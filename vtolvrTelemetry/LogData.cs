using System;
using System.Collections.Generic;

namespace vtolvrTelemetry
{
    public class LogData
    {

        public string Location { get; set; }
        public Int32 unixTimestamp { get; set; }

        public VehicleClass Vehicle { get; set; } = new VehicleClass();
        public PhysicsClass Physics { get; set; } = new PhysicsClass();

        public class PhysicsClass
        {
            public string XAccel { get; set; }
            public string YAccel { get; set; }
            public string ZAccel { get; set; }
            public string PlayerGs { get; set; }
        }

        public class VehicleClass
        {
            public string IsLanded { get; set; }
            public string Drag { get; set; }
            public string Mass { get; set; }
            public string VehicleName { get; set; }
            public string Airspeed { get; set; }
            public string VerticalSpeed { get; set; }
            public string AltitudeASL { get; set; }
            public string Heading { get; set; }
            public string Pitch { get; set; }
            public string AoA { get; set; }
            public string Roll { get; set; }
            public string TailHook { get; set; }
            public string Health { get; set; }
            public string Flaps { get; set; }
            public string Brakes { get; set; }
            public string GearState { get; set; }
            public string RadarCrossSection { get; set; }
            public string BatteryLevel { get; set; }
            public List<Dictionary<string, string>> Engines { get; set; }
            public string EjectionState { get; set; }
            public Dictionary<string, string> Lights { get; set; }


            public FuelClass Fuel { get; set; } = new FuelClass();
            public AvionicsClass Avionics { get; set; } = new AvionicsClass();


            public class AvionicsClass
            {
                public string StallDetector { get; set; }
                public string MissileDetected { get; set; }
                public string RadarState { get; set; }
                public List<Dictionary<string, string>> RWRContacts { get; set; }
                public string masterArm { get; set; }

            }

            public class FuelClass
            {
                public string FuelLevel { get; set; }
                public string FuelBurnRate { get; set; }
                public string FuelDensity { get; set; }
            }


        }

        public string CSVHeaders()
        {
            return "Timestamp,VehicleName,Mass,Drag,AltitudeASL,Airspeed,Roll,Pitch,Heading,AoA,XAccel,YAccel,ZAccel,PlayerGs,FuelDensity,FuelBurnRate,FuelLevel,RadarCrossSection,BatteryLevel,Health,Stall";
        }
        public string ToCSV()
        {
            return $"{this.unixTimestamp},{this.Vehicle.VehicleName},{this.Vehicle.Mass},{this.Vehicle.Drag},{this.Vehicle.AltitudeASL},{this.Vehicle.Airspeed},{this.Vehicle.Roll},{this.Vehicle.Pitch},{this.Vehicle.Heading},{this.Vehicle.AoA},{this.Physics.XAccel},{this.Physics.YAccel},{this.Physics.ZAccel},{this.Physics.PlayerGs},{this.Vehicle.Fuel.FuelDensity},{this.Vehicle.Fuel.FuelBurnRate},{this.Vehicle.Fuel.FuelLevel},{this.Vehicle.RadarCrossSection},{this.Vehicle.BatteryLevel},{this.Vehicle.Health},{this.Vehicle.Avionics.StallDetector}";
        }
    }
}
