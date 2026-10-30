import { api } from "@/lib/api";
import { PageHeader } from "@/components/page-header";
import { CategoryList } from "@/components/catalog/category-list";
import { NoFamilyNotice } from "@/components/no-family-notice";

export const metadata = { title: "Catalogus - PakEchtMee" };

export default async function CatalogPage() {
  const me = await api.me();

  if (me && !me.family) {
    return (
      <>
        <PageHeader title="Catalogus" description="Categorieën en spullen die je steeds opnieuw gebruikt." />
        <main className="px-4">
          <NoFamilyNotice />
        </main>
      </>
    );
  }

  // Switched-off general categories are listed too, so they can be switched back on here.
  const categories = await api.listCategories(true, true);

  return (
    <>
      <PageHeader title="Catalogus" description="Categorieën en spullen die je steeds opnieuw gebruikt." />
      <main className="px-4">
        <CategoryList categories={categories} />
      </main>
    </>
  );
}
