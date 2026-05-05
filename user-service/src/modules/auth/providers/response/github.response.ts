
export interface GithubProfile {
    id:          string;
    nodeId:      string;
    displayName: string;
    username:    string;
    profileUrl:  string;
    photos:      Email[];
    provider:    string;
    _raw:        string;
    _json:       JSON;
    emails:      Email[];
}

interface JSON {
    login:               string;
    id:                  number;
    location:            string;
    bio:                 string;
}

interface Email {
    value: string;
}