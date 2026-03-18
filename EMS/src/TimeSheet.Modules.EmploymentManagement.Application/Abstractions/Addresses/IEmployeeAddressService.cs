using CreateCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Create.Command;
using CreateResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Create.Result;
using DeleteCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Delete.Command;
using DeleteOwnCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.DeleteOwn.Command;
using GetByIdAddress = TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById.Address;
using GetByIdQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.GetById.Query;
using ListMineQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.ListMine.Query;
using ListQuery = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Query;
using ListResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.List.Result;
using SetOwnPrimaryCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetOwnPrimary.Command;
using SetPrimaryCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.SetPrimary.Command;
using UpdateCommand = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update.Command;
using UpdateResult = TimeSheet.Modules.EmploymentManagement.Application.Addresses.Update.Result;

namespace TimeSheet.Modules.EmploymentManagement.Application.Abstractions.Addresses;

public interface IEmployeeAddressService
{
    Task<ListResult> ListAsync(ListQuery query, CancellationToken cancellationToken);

    Task<GetByIdAddress?> GetAsync(GetByIdQuery query, CancellationToken cancellationToken);

    Task<CreateResult> CreateAsync(CreateCommand command, CancellationToken cancellationToken);

    Task<UpdateResult> UpdateAsync(UpdateCommand command, CancellationToken cancellationToken);

    Task SetPrimaryAsync(SetPrimaryCommand command, CancellationToken cancellationToken);

    Task DeleteAsync(DeleteCommand command, CancellationToken cancellationToken);

    Task<ListResult> ListOwnAsync(ListMineQuery query, CancellationToken cancellationToken);

    Task SetOwnPrimaryAsync(SetOwnPrimaryCommand command, CancellationToken cancellationToken);

    Task DeleteOwnAsync(DeleteOwnCommand command, CancellationToken cancellationToken);
}
