namespace Assistant.PushBullet.Interfaces {
	public interface IDevice {

		bool CurrentlyActive { get; set; }

		string Identifier { get; set; }

		float Created { get; set; }

		float LastModified { get; set; }

		string DeviceType { get; set; }

		string DeviceKind { get; set; }

		string DeviceNickname { get; set; }

		string DeviceManufacturer { get; set; }

		string DeviceModel { get; set; }

		int AppVersion { get; set; }

		bool Pushable { get; set; }

		string Icon { get; set; }

		string DeviceFingerprint { get; set; }

		string PushToken { get; set; }

		bool HasSMSAbility { get; set; }

		bool HasMMSAbility { get; set; }

		string RemoteFiles { get; set; }

	}
}
