using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Core
{
    public interface IUpdatable
    {
        Task UpdateAsync(CancellationToken ct);
    }
}