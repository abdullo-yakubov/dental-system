namespace DentalLocal.Infrastructure.Security;

/// <summary>
/// Источник ключевого материала для автоматических (не PIN-защищённых) операций, например
/// шифрования ежедневных автобэкапов. Секрет генерируется один раз и хранится на диске
/// зашифрованным через DPAPI (машинный секрет), а не как строка в исходном коде.
/// </summary>
public interface IMachineSecretProvider
{
    byte[] GetOrCreateAutoBackupSecret();
}
