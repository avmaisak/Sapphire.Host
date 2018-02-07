namespace Sapphire.Host.Core.Entity
{
	public interface IDeviceResponse
	{
		/// <summary>
		/// Номер строки
		/// </summary>
		long LineNumber { get; set; }
		/// <summary>
		/// Успех
		/// </summary>
		bool Success { get; set; }
		/// <summary>
		/// Пульс
		/// </summary>
		bool Wait { get; set; }
		/// <summary>
		/// Сырые данные
		/// </summary>
		string RawData { get; set; }
	}
}