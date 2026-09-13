namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Specifies how work-context retrieval failures are handled.</summary>
public enum WorkContextErrorBehavior
{
    /// <summary>Returns successful facets alongside sanitized facet failures.</summary>
    BestEffort,

    /// <summary>Throws when an enabled operation encounters a real failure.</summary>
    FailFast,
}

/// <summary>Identifies a Microsoft 365 work-context facet.</summary>
public enum WorkContextFacet
{
    /// <summary>The signed-in user's profile.</summary>
    UserProfile,

    /// <summary>The signed-in user's immediate manager.</summary>
    Manager,

    /// <summary>The signed-in user's mailbox work settings.</summary>
    WorkSettings,

    /// <summary>The signed-in user's default calendar view.</summary>
    Calendar,
}

/// <summary>Describes the outcome of retrieving a work-context facet.</summary>
public enum WorkContextFacetStatus
{
    /// <summary>The facet was disabled and no operation was attempted.</summary>
    Disabled,

    /// <summary>The facet was retrieved successfully.</summary>
    Available,

    /// <summary>Microsoft Graph reported a documented expected absence.</summary>
    Unavailable,

    /// <summary>The facet could not be retrieved or validated.</summary>
    Failed,
}

/// <summary>Classifies a sanitized work-context retrieval failure.</summary>
public enum WorkContextFailureKind
{
    /// <summary>The delegated access token could not be acquired.</summary>
    TokenAcquisition,

    /// <summary>The request could not reach Microsoft Graph.</summary>
    Transport,

    /// <summary>Microsoft Graph rejected authentication.</summary>
    Authentication,

    /// <summary>Microsoft Graph rejected authorization.</summary>
    Authorization,

    /// <summary>Microsoft Graph throttled the request.</summary>
    Throttled,

    /// <summary>Microsoft Graph returned another service failure.</summary>
    Service,

    /// <summary>Microsoft Graph returned an unusable successful response.</summary>
    InvalidResponse,
}