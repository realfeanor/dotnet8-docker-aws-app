# Stockroom frontend

React frontend for the .NET product catalog API. The interface is in Turkish and supports registration, login, searchable products, category filters, and product/category management.

## Run

Use Node.js 24. Copy `.env.example` to `.env`, set `VITE_API_URL` to the backend origin (without `/api`), then run `npm ci` and `npm start`.

Run `npm test` for permission and route tests, and `npm run build` for a production build. Vite writes the production application to `dist/`.

The container serves that build through Nginx. Its `try_files` fallback returns `index.html` for routes such as `/products`, allowing React Router to handle direct navigation and browser refreshes.

## Permissions

New registrations receive `Product.Get` and `Category.Get` from the backend. No role can be selected during registration.

| Claim | Frontend capability |
| --- | --- |
| Product.Get | Product page and navigation |
| Category.Get | Category page and navigation, product category filter |
| Product.Add / Update / Delete | Corresponding product controls |
| Category.Add / Update / Delete | Corresponding category controls |
| Admin | All of the above |

Provision admin permissions through the backend/database; registration never grants them. Sign in again after changing claims to get a new token.

The frontend reads the backend's .NET JWT role claims to render navigation and controls. It guards routes, restores valid sessions, signs out at token expiry or authenticated API 401 responses, and synchronizes sessions between tabs. Token decoding in the browser is for presentation; the backend validates tokens and enforces permissions.

Product lists use `GET Products/getall`; category lists use `POST Category/getallcategories`. Management uses the existing add/update POST endpoints and DELETE endpoints. Backend validation errors are displayed in the forms. Prices are shown without a currency symbol because the API does not specify a currency.
