export class IConfig {
    constructor(token: string) {
        this.authToken = token;
    }

    authToken: string;
}

export class ApiClientBase {
    private readonly config: IConfig;

    protected constructor(config: IConfig) {
        this.config = config;
    }

    protected transformOptions = (options: RequestInit): Promise<RequestInit> => {
        options.headers = {
            ...options.headers,
            Authorization: "Bearer " + this.config.authToken
        };
        return Promise.resolve(options);
    };
}