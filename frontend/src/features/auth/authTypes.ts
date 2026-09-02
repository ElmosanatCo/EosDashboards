export type AuthenticatedUser = {
  id: number;
  accountName: string;
  firstName: string;
  lastName: string;
  roleIds: number[];
};

export type Challenge = {
  challengeToken: string;
  maskedMobile: string;
  expiresAtUtc: string;
  resendAvailableAtUtc: string;
};

export type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  sessionExpiresAtUtc: string;
  user: AuthenticatedUser;
};
