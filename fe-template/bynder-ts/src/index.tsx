import ReactDOM from "react-dom/client";
import "./index.css";
import App from "./App";
import { PrimeReactProvider } from "primereact/api";

const rootElement = document.getElementById("root");

if (rootElement) {
  const root = ReactDOM.createRoot(rootElement);
  const modalType = rootElement.getAttribute("data-type");

  root.render(
    <PrimeReactProvider>
      <App modalType={modalType} />
    </PrimeReactProvider>
  );
} else {
  Error("No Root Element");
}
