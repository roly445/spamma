import { Result } from '../monads/result';

export class XhrHelper {
    public static async PostJsonInternalOfT<TResult>(
        url: string,
        data: any) : Promise<Result<TResult, Error>> {
        try {
            const options: RequestInit = {
                method: 'POST',
                mode: 'same-origin',
                cache: 'no-cache',
                credentials: 'same-origin',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            };
            if (data) {
                options.body = JSON.stringify(data);
            }
            const response = await fetch(url, options);
            const actualResponse = await response.json() as TResult;
            return Result.Ok<TResult, Error>(actualResponse);
        } catch (e) {
            return Result.Fail<TResult, Error>(e);
        }
    }

    public static async PostInternalOfT<TResult>(
        url: string) : Promise<Result<TResult, Error>> {
        return this.PostJsonInternalOfT<TResult>(url, null);
    }
}