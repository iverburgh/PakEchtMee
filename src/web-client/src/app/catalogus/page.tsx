import { api } from "@/lib/api";
import { PageHeader } from "@/components/page-header";
import { CategoryList } from "@/components/catalog/category-list";

export const metadata = { title: "Catalogus - PakEchtMee" };

export default async function CatalogPage() {
  const categories = await api.listCategories(true);

  return (
    <>
      <PageHeader title="Catalogus" description="Categorieën en spullen die je steeds opnieuw gebruikt." />
      <main className="px-4">
        <CategoryList categories={categories} />
      </main>
    </>
  );
}
