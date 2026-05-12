namespace OpenTaiko.Shrandy.Utilities
{
	public static class ContainerHelpers
	{
		public static List<T> Shuffle<T>(this List<T> list)
		{
			Random rng = new();
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return list;
		}
	}
}