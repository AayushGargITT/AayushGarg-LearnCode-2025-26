import { HttpInterceptorFn } from '@angular/common/http';

const tokenStorageKey = 'resourceMindToken';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = sessionStorage.getItem(tokenStorageKey);
  if (!token) {
    return next(req);
  }

  return next(req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  }));
};
