using DentalLocal.Domain.Entities;

namespace DentalLocal.Application.Common;

public interface IPdfService
{
    byte[] GenerateAgreementPdf(Patient patient);
    
    byte[] GenerateConsentPdf(Patient patient);
    
    byte[] GeneratePrescriptionPdf(Visit visit);
    
    byte[] GenerateInvoicePdf(Visit visit);
}
