# WMS Frontend

Angular frontend for the Warehouse Management System.

## Run locally

The frontend expects the backend API to be available on `http://localhost:5021`.

From this folder:

```bash
npm install
npm start
```

Open `http://localhost:4200`.

## Quality checks

```bash
npm run format:check
npm run lint
npm run build
npm run test:ci
```

## Notes

- `npm start` uses `proxy.conf.json` so `/api` and `/swagger` requests go to the backend.
- Role selection is stored in local storage and sent as the `X-Wms-Role` header on API requests.
- The UI assumes the backend database schema has already been created and migrated.
