using FluentScheduler;
using System;

namespace Luna.Features.Remainders {
	public class Remainder : IJob {
		public readonly string? UniqueId;
		public readonly string? Message;
		public readonly DateTime RemaindAt;
		public readonly Action<Remainder>? Func;
		public readonly bool IsCompleted;

		public Remainder() { }

		public Remainder(string? msg, DateTime at, Action<Remainder>? func = null) {
			Message = msg;
			UniqueId = Guid.NewGuid().ToString();
			RemaindAt = at;
			Func = func;
			IsCompleted = false;
		}

		public void Execute() {
			if(Func != null) {
				Func.Invoke(this);
			}
		}
	}
}
