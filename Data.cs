using System.Collections.Generic;

namespace ModContentDiffer
{
	/// <summary>
	/// Used by json
	/// </summary>
	public class Data
	{
		/// <summary>
		/// Stores a list of names for each content category
		/// </summary>
		public Dictionary<string, List<string>> content;

		public Data()
		{
			content = new Dictionary<string, List<string>>();
		}

		/// <summary>
		/// Safely adds a name for the given content category
		/// </summary>
		public void Add(string contentCategory, string name)
		{
			if (!this.content.ContainsKey(contentCategory))
			{
				this.content[contentCategory] = new List<string>();
			}

			var list = this.content[contentCategory];

			if (!list.Contains(name))
			{
				list.Add(name);
			}
		}

		/// <summary>
		/// Merges another data set into this one
		/// </summary>
		public void MergeWith(Data otherData)
		{
			Dictionary<string, List<string>> otherContent = otherData.content;

			foreach (var pair in otherContent)
			{
				string contentCategory = pair.Key;

				foreach (string name in otherContent[contentCategory])
				{
					this.Add(contentCategory, name);
				}
			}
		}
	}
}
