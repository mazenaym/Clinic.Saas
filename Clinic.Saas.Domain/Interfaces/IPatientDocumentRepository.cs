using Clinic.Saas.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Interfaces
{
    public interface IPatientDocumentRepository
    {
        Task AddAsync(PatientDocument document);
    }
}
