export const AuthService = {
    authApiUrl: 'http://localhost:1235',
    loginPath: '/v1/auth/login',
    getLoginHref: (redirectUrl: string) => {
        return `${AuthService.authApiUrl}${AuthService.loginPath}?returnUrl=${encodeURIComponent(redirectUrl)}`;
    }
}