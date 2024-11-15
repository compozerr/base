import React from "react";

interface Props {
   name: string;
}

const ExampleComponent = (props: Props) => {
   return (
      <div>
         Hello {props.name}
      </div>
   );
}

export default ExampleComponent;