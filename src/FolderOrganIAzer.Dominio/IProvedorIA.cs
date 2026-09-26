using System.Threading.Tasks;

namespace FolderOrganIAzer.Dominio;

public interface IProvedorIA
{
    Task<bool> TestarDisponibilidadeAsync();
}
