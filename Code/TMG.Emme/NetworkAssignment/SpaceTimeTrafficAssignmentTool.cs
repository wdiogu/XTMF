/*
    Copyright 2023 Travel Modelling Group, Department of Civil Engineering, University of Toronto

    This file is part of XTMF.

    XTMF is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    XTMF is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with XTMF.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using XTMF;
using System.IO;
using System.Buffers.Text;

namespace TMG.Emme.NetworkAssignment
{
    [ModuleInformation(Description =
        @"The he Space-time Traffic Assignment Tool or STTA runs a multi-class quasi-dynamic traffic assignment that uses a time-dependent network loading.",
        Name = "Space Time Traffic Assignment Tool"
        )]

    public class SpaceTimeTrafficAssignmentTool : IEmmeTool
    {
        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        [RunParameter("Scenario Number", 1, "The scenario number to execute against.")]
        public int ScenarioNumber;

        const string ToolName = "tmg.XTMF_internal.space_time_travel_assignment";

        [SubModelInformation(Description = "The classes for this multi-class assignment.")]
        public TrafficClass[] TrafficClasses;

        [RunParameter("Interval Lengths", "60,60,60", "Defines how the assignment time is split into intervals.")]
        public string IntervalLengths;

        [RunParameter("Start Time", "00:00", "Start time of the assignment period. String format hh:mm")]
        public string StartTime;

        [RunParameter("Extra Time Interval", 60, "Total time which the flow in the network will be allowed to clear. NOTE: no additional result attributes are created for the time interval")]
        public float ExtraTimeInterval;

        [RunParameter("Number of Extra Time Intervals", 2, "Total number of time periods. Cannot be greater than 125. Default is 2")]
        public int NumberOfExtraTimeIntervals;

        [RunParameter("Background Traffic Link Component Extra Attribute", "@tvph", "Time dependent background traffic link extra attribute")]
        public string LinkComponentAttribute;

        [RunParameter("Create Link Component Extra Attribute", false, "Set to true if time dependent background traffic link extra attribute does not already exist in the network")]
        public bool CreateLinkComponentAttribute;

        [RunParameter("Start Index for Attributes", 1, "Time Dependent Start Indices used to create the alphanumerical attribute name string for attributes in this class.")]
        public int StartIndex;

        [RunParameter("Max Inner Iterations", 15, "Maximum inner iterations")]
        public int InnerIterations;

        [RunParameter("Max Outer Iterations", 5, "Maximum outer iterations")]
        public int OuterIterations;

        [RunParameter("Coarse Relative Gap", 0.01f, "Coarse relative gap")]
        public float CoarseRGap;

        [RunParameter("Fine Relative Gap", 0.0001f, "Fine relative gap")]
        public float FineRGap;

        [RunParameter("Coarse Best Relative Gap", 0.01f, "Coarse best relative gap")]
        public float CoarseBRGap;

        [RunParameter("Fine Best Relative Gap", 0.0001f, "Fine best relative gap")]
        public float FineBRGap;

        [RunParameter("Normalized Gap", 0.005f, "The minimum gap required to terminate the algorithm.")]
        public float NormalizedGap;

        [RunParameter("Performance Flag", true, "Set this to false to leave a free core for other work, recommended to leave set to true.")]
        public bool PerformanceFlag;

        [RunParameter("Run Title", "Multi-class Run", "The name of the run to appear in the logbook.")]
        public string RunTitle;

        [ModuleInformation(Description = "This module is used to define the different classes that will be assigned during STTA.")]
        public sealed class TrafficClass : IModule
        {
            [RunParameter("Mode", 'c', "The mode for this class.")]
            public char Mode;

            [RunParameter("Demand Matrix", 1000, "The id of the demand matrix to use.")]
            public int DemandMatrixNumber;

            [RunParameter("Time Matrix", 0, "The matrix number to save in vehicle travel times")]
            public int TimeMatrixNumber;

            [RunParameter("Cost Matrix", 0, "The matrix number to save the total cost into.")]
            public int CostMatrixNumber;

            [RunParameter("Toll Matrix", 0, "The matrix to save the toll costs into.")]
            public int TollMatrixNumber;

            [RunParameter("VolumeAttribute", "@auto_volume", "The name of the attribute to save the volumes into (or None for no saving).")]
            public string VolumeAttribute;

            [RunParameter("Class Start Index for Attributes", 1, "")]
            public int AttributeStartIndex;

            [RunParameter("TollAttributeID", "@toll", "The attribute containing the road tolls for this class of vehicle.")]
            public string LinkTollAttributeID;

            [RunParameter("Toll Weights", "1,2,3", "")]
            public string TollWeight;

            [RunParameter("LinkCost", 0f, "The penalty in minutes per dollar to apply when traversing a link.")]
            public float LinkCost;
            public string Name { get; set; }

            public float Progress { get; set; }

            public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

            public bool RuntimeValidation(ref string error)
            {
                if (char.IsLetter(Mode))
                {
                    error = "In '" + Name + "' the Mode '" + Mode + "' is not a feasible mode for multi class assignment!";
                    return true;
                }
                return false;
            }

            [SubModelInformation(Required = false, Description = " Path analysis specification")]
            public Analysis[] PathAnalyses;
            public class Analysis : IModule
            {
                public string Name { get; set; }

                public float Progress => 0f;

                public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50, 150, 50);
                public bool RuntimeValidation(ref string error)
                {
                    return true;
                }
            }
        }

        public bool Execute(Controller controller)
        {
            var mc = controller as ModellerController;
            if (mc == null)
            {
                throw new XTMFRuntimeException(this, "TMG.Emme.NetworkAssignment.SpaceTimeTrafficAssignmentTool requires the use of EMME Modeller and will not work through command prompt!");
            }

            string ret = null;
            if (!mc.CheckToolExists(this, ToolName))
            {
                throw new XTMFRuntimeException(this, "There was no tool with the name '" + ToolName + "' available in the EMME databank!");
            }

            return mc.Run(this, ToolName, GetParameters(), (p) => Progress = p, ref ret);
        }

        private ModellerControllerParameter[] GetParameters()
        {
            // Build the traffic class JSON parameter
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = false });
            writer.WriteStartObject();
            writer.WritePropertyName("TrafficClasses");
            writer.WriteStartArray();
            foreach (var trafficClass in TrafficClasses)
            {
                writer.WriteStartObject();
                writer.WriteString("VolumeAttribute", trafficClass.VolumeAttribute);
                writer.WriteNumber("AttributeStartIndex", trafficClass.AttributeStartIndex);
                writer.WriteString("LinkTollAttributeID", trafficClass.LinkTollAttributeID);
                writer.WriteString("TollWeightList", trafficClass.TollWeight);
                writer.WriteNumber("LinkCost", trafficClass.LinkCost);
                writer.WriteString("Mode", trafficClass.Mode.ToString());
                writer.WriteNumber("DemandMatrixNumber", trafficClass.DemandMatrixNumber);
                writer.WriteNumber("TimeMatrixNumber", trafficClass.TimeMatrixNumber);
                writer.WriteNumber("CostMatrixNumber", trafficClass.CostMatrixNumber);
                writer.WriteNumber("TollMatrixNumber", trafficClass.TollMatrixNumber);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            stream.Position = 0;
            // convert the traffic class data into a string and send it through the bridge as parameters
            var sb = System.Text.UTF8Encoding.UTF8.GetString(stream.ToArray());
            return new[]
            {
                new ModellerControllerParameter("ScenarioNumber", ScenarioNumber.ToString()),
                new ModellerControllerParameter("IntervalLengths", IntervalLengths.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("StartTime", StartTime.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("ExtraTimeInterval", ExtraTimeInterval.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("NumberOfExtraTimeIntervals", NumberOfExtraTimeIntervals.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("LinkComponentAttribute", LinkComponentAttribute),
                new ModellerControllerParameter("CreateLinkComponentAttribute", CreateLinkComponentAttribute.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("StartIndex", StartIndex.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("InnerIterations", InnerIterations.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("OuterIterations", OuterIterations.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("CoarseRGap",CoarseRGap.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("FineRGap",FineRGap.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("CoarseBRGap", CoarseBRGap.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("FineBRGap",FineBRGap.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("NormalizedGap", NormalizedGap.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("PerformanceFlag", PerformanceFlag.ToString(CultureInfo.InvariantCulture)),
                new ModellerControllerParameter("RunTitle", RunTitle),
                new ModellerControllerParameter("TrafficClasses", sb.ToString()),
            };
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }

}
