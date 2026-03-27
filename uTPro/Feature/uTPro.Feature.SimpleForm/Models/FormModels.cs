using NPoco;

namespace uTPro.Feature.SimpleForm.Models;

// ── Database DTOs ──

[TableName("utpro_SimpleForm")]
[PrimaryKey("Id", AutoIncrement = true)]
public class SimpleFormDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string? FieldsJson { get; set; }
    public string? SuccessMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public string? EmailTo { get; set; }
    public string? EmailSubject { get; set; }
    public bool StoreSubmissions { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

[TableName("utpro_SimpleFormSubmission")]
[PrimaryKey("Id", AutoIncrement = true)]
public class SimpleFormSubmissionDto
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public string? DataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedUtc { get; set; }
}

// ── API ViewModels ──

public class FormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public List<FormFieldViewModel> Fields { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public string? EmailTo { get; set; }
    public string? EmailSubject { get; set; }
    public bool StoreSubmissions { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class FormFieldViewModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "text";
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? CssClass { get; set; }
    public bool Required { get; set; }
    public string? Validation { get; set; }
    public string? ValidationMessage { get; set; }
    public string? DefaultValue { get; set; }
    public List<OptionItem>? Options { get; set; }
    public int SortOrder { get; set; }
    /// <summary>1 = half width (default), 2 = full width in 2-col layout</summary>
    public int ColSpan { get; set; } = 1;
    /// <summary>Extra attributes JSON for custom field types (e.g. {"siteKey":"xxx"} for turnstile)</summary>
    public Dictionary<string, string>? Attributes { get; set; }
}

public class OptionItem
{
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SaveFormRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public List<FormFieldViewModel> Fields { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public string? EmailTo { get; set; }
    public string? EmailSubject { get; set; }
    public bool StoreSubmissions { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
}

public class DeleteFormRequest
{
    public int Id { get; set; }
}

public class GetFormRequest
{
    public int Id { get; set; }
}

public class SubmitFormRequest
{
    public string Alias { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = [];
}

public class SubmissionViewModel
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public Dictionary<string, string> Data { get; set; } = [];
    public string? IpAddress { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class SubmissionListRequest
{
    public int FormId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Total { get; set; }
}
