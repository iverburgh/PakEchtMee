import { notFound } from "next/navigation";
import Link from "next/link";
import { ChevronLeft } from "lucide-react";
import { api } from "@/lib/api";
import { AdministratorOnly } from "@/components/admin/administrator-only";
import { ItemList } from "@/components/catalog/item-list";

export default async function GeneralCategoryItemsPage({ params }: PageProps<"/beheer/catalogus/[categoryId]">) {
  const { categoryId } = await params;
  const me = await api.me();

  if (!me?.isAdministrator) {
    return <AdministratorOnly />;
  }

  const [categories, items] = await Promise.all([api.listGeneralCategories(true), api.listItems(categoryId, true)]);
  const category = categories.find((candidate) => candidate.id === categoryId);

  if (!category) {
    notFound();
  }

  return (
    <>
      <header className="flex items-center gap-2 px-2 pt-4 pb-2">
        <Link
          href="/beheer/catalogus"
          className="text-muted-foreground hover:text-foreground flex size-11 items-center justify-center rounded-md"
          aria-label="Terug naar de algemene catalogus"
        >
          <ChevronLeft aria-hidden />
        </Link>
        <h1 className="truncate text-xl font-semibold tracking-tight">{category.name}</h1>
      </header>

      <main className="px-4">
        <ItemList
          categoryId={categoryId}
          items={items}
          categories={categories.filter((candidate) => !candidate.isDeleted)}
        />
      </main>
    </>
  );
}
