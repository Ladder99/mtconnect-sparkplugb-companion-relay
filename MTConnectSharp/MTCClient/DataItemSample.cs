using System;

namespace MTConnectSharp
{
   /// <summary>
   /// A single value from a current or sample response
   /// </summary>
   public class DataItemSample : MTConnectSharp.IDataItemSample
	{
		/// <summary>
		/// The value of the sample
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// The timestamp of the sample
		/// </summary>
		public DateTime TimeStamp { get; set; }
		
		public long Sequence { get; set; }

		/// <summary>
		/// Creates a new sample with the current time as the timestamp
		/// </summary>
		/// <param name="value">Value of the sample</param>
		internal DataItemSample(string value)
		{
			TimeStamp = DateTime.Now;
			Value = value;
		}

		/// <summary>
		/// Creates a new sample
		/// </summary>
		/// <param name="value">Value of the sample</param>
		/// <param name="timestamp">Timestamp of the sample</param>
		internal DataItemSample(string value, DateTime timestamp)
		{
			TimeStamp = timestamp;
			Value = value;
		}
		
		internal DataItemSample(string value, DateTime timestamp, long sequence)
		{
			TimeStamp = timestamp;
			Value = value;
			Sequence = sequence;
		}

		/// <summary>
		/// Returns the Value
		/// </summary>
		public override string ToString()
		{
			return Value;
		}
	}

}

