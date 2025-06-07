import React from "react";

interface Props {
   name: string;
}

const StripeComponent = (props: Props) => {
   return (
      <div>
         Hello {props.name}
      </div>
   );
}

export default StripeComponent;