export type StructureSubrowsData = {
  Subrows: any[];
  id: string;
  [key: string]: any;
};

const makeStructureData = (arr: any[], structureKey: string) => {
  const structureData: StructureSubrowsData[] = arr.reduce(
    (accumulator: StructureSubrowsData[], currentElement: any) => {
      if (!accumulator.length) {
        //create arr
        accumulator.push({
          [structureKey]: currentElement[structureKey],
          // ...currentElement,
          Subrows: [
            {
              ...currentElement,
              groupId: `${String(accumulator.length)}.0`,
              parentId: `${String(accumulator.length)}`,
            },
          ],
          id: String(accumulator.length),
        });
      } else {
        let i = 0;
        if (
          accumulator.some((item, index) => {
            //check item existence
            if (item[structureKey] === currentElement[structureKey]) {
              i = index;
              return true;
            } else {
              return false;
            }
          })
        ) {
          //add sub item
          accumulator[i]?.Subrows.push({
            ...currentElement,
            groupId: `${String(i)}.${accumulator[i]?.Subrows.length}`,
            parentId: `${String(i)}`,
          });
        } else {
          //create new item
          accumulator.push({
            [structureKey]: currentElement[structureKey],
            // ...currentElement,
            Subrows: [
              {
                ...currentElement,
                groupId: `${String(accumulator.length)}.0`,
                parentId: `${String(accumulator.length)}`,
              },
            ],
            id: String(accumulator.length),
          });
        }
      }
      return accumulator;
    },
    []
  );

  return structureData;
};

const canUseDOM = () => {
  return !!(
    typeof window !== "undefined" &&
    window.document &&
    window.document.createElement
  );
};

const getUrlAPI = () => {
  if (canUseDOM()) {
    return window.location.host.includes("localhost")
      ? "https://contentworkflowsc.dev.local"
      : "";
  } else {
    return "";
  }
};

interface StringIndexedArray extends Array<any> {
  [index: string]: any;
}

const getUrlVars = (): StringIndexedArray => {
  const vars: StringIndexedArray = [];
  let hash: StringIndexedArray;
  const hashes = window.location.href
    .slice(window.location.href.indexOf("?") + 1)
    .split("&");
  for (let i = 0; i < hashes.length; i++) {
    hash = hashes[i].split("=");
    vars.push(hash[0]);
    vars[hash[0]] = hash[1];
  }
  return vars;
};

interface Indexable {
  [structureKey: string]: any; // This is the index signature
}
const findItem = <T extends Indexable>(
  prop: string,
  array: T[] | undefined,
  property: string | undefined
) => {
  return array?.find((item: T) => item[prop] === property);
};

export { makeStructureData, canUseDOM, getUrlAPI, getUrlVars, findItem };
