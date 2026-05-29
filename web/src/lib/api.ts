import { createApiClient } from "@finans/shared";

// Geliştirmede Vite proxy `/api`'yi .NET backend'e yönlendirir; prod'da reverse
// proxy aynı origin'den servis eder → taban URL boş bırakılır.
export const api = createApiClient({ baseUrl: "" });
