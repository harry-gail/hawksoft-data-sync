using System.Text.Json.Serialization;

namespace HawksoftDataSync.Models;

public class ClientResponse
{
    [JsonPropertyName("clientNumber")]
    public int ClientNumber { get; set; }

    [JsonPropertyName("details")]
    public ClientDetails? Details { get; set; }

    [JsonPropertyName("people")]
    public List<Person>? People { get; set; }

    [JsonPropertyName("contacts")]
    public List<Contact>? Contacts { get; set; }
}

public class ClientDetails
{
    [JsonPropertyName("officeId")]
    public int OfficeId { get; set; }

    [JsonPropertyName("clientType")]
    public string? ClientType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("isPersonal")]
    public bool IsPersonal { get; set; }

    [JsonPropertyName("isCommercial")]
    public bool IsCommercial { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("agencyNum")]
    public string? AgencyNum { get; set; }

    [JsonPropertyName("clientSince")]
    public DateTime? ClientSince { get; set; }

    [JsonPropertyName("contactInfoLastUpdated")]
    public DateTime? ContactInfoLastUpdated { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("modified")]
    public DateTime? Modified { get; set; }
}

public class Person
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("relationship")]
    public string? Relationship { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("occupation")]
    public string? Occupation { get; set; }

    [JsonPropertyName("maritalStatus")]
    public string? MaritalStatus { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("modified")]
    public DateTime? Modified { get; set; }
}

public class Contact
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("personId")]
    public string? PersonId { get; set; }

    [JsonPropertyName("allowMassEmail")]
    public bool AllowMassEmail { get; set; }

    [JsonPropertyName("isBillingContact")]
    public bool IsBillingContact { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("modified")]
    public DateTime? Modified { get; set; }
}

public class PhoneNumberSync
{
    public int ClientNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhoneType { get; set; }
    public string? PersonId { get; set; }
    public string? PersonName { get; set; }
    public int Priority { get; set; }
    public DateTime? LastModified { get; set; }
}