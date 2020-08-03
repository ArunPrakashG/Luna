using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Extensions {
	internal static class HelperFunctions {
		public static void ForEach(this int[] array, Action<int> onEachElement) {
			if (array.Length <= 0 || onEachElement == null) {
				return;
			}

			for(int i = 0; i < array.Length; i++) {
				onEachElement.Invoke(array[i]);
			}
		}
	}
}
