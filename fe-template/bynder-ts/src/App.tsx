import { lazy, Suspense } from "react";
import "./App.scss";

const Mappings = lazy(
  () =>
    import(/*webpackChunkName: "Mappings" */ "./components/Mappings/Mappings")
);
const AddOrUpdateMapping = lazy(
  () =>
    import(
      /*webpackChunkName: "AddOrUpdateMapping" */ "./components/Mappings/AddOrUpdateMapping"
    )
);
const Import = lazy(
  () => import(/*webpackChunkName: "Import" */ "./components/Import/Import")
);
const MultiLocationImport = lazy(
  () =>
    import(
      /*webpackChunkName: "MultiLocationImport" */ "./components/Import/MultiLocationImport"
    )
);
const Update = lazy(
  () => import(/*webpackChunkName: "Update" */ "./components/Update/Update")
);

function App(props: { modalType: string | null }) {
  const { modalType } = props;

  if (modalType) {
    return (
      <Suspense>
        {modalType === "mapping" && <Mappings></Mappings>}
        {modalType === "manage-mapping" && (
          <AddOrUpdateMapping></AddOrUpdateMapping>
        )}
        {modalType === "import" && <Import></Import>}
        {modalType === "multi-location-import" && (
          <MultiLocationImport></MultiLocationImport>
        )}
        {modalType === "update" && <Update></Update>}
      </Suspense>
    );
  }

  return <div className="App"></div>;
}

export default App;
