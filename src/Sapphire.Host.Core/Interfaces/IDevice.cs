using System;

namespace Sapphire.Host.Core.Interfaces
{
	/// <inheritdoc />
	/// <summary>
	/// Интерфейс устройства
	/// </summary>
	public interface IDevice : IDisposable
	{
		/// <summary>
		/// Инициализация
		/// </summary>
		void Init();
		/// <summary>
		/// Выполнить соединение
		/// </summary>
		void Connect(int timeOut = 3000);
		/// <summary>
		/// Разорвать соединение
		/// </summary>
		/// <param name="force">Принудительно</param>
		void Disconnect(bool force = false);
		/// <summary>
		/// Отправка данных на устройство
		/// </summary>
		/// <param name="frame"></param>
		void SendCommandFrame(string frame);
		/// <summary>
		/// Ответ
		/// </summary>
		/// <returns></returns>
		string GetResponse();
	}
}