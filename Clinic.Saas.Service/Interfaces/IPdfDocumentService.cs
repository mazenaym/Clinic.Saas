namespace Clinic.Saas.Service.Interfaces;

public interface IPdfDocumentService
{
    byte[] Generate(string title, IEnumerable<(string Label, string Value)> fields, IEnumerable<string>? lines = null);
}
