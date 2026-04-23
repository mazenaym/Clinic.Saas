using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        //IVisitRepository Visits { get; }
        //IPrescriptionRepository Prescriptions { get; }
        //IVitalSignsRepository VitalSigns { get; }
        //IAuditLogRepository AuditLogs { get; }

        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

    }
}
